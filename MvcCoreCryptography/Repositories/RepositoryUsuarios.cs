using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.IdentityModel.Tokens;
using MvcCoreCryptography.Data;
using MvcCoreCryptography.Helpers;
using MvcCoreCryptography.Models;

namespace MvcCoreCryptography.Repositories
{
    public class RepositoryUsuarios
    {
        private UsuariosContext context;

        public RepositoryUsuarios(UsuariosContext context)
        {
            this.context = context;
        }

        private async Task<int> GetMaxIdUsuarioAsync()
        {
            if (this.context.Usuarios.Count() == 0)
            {
                return 1;
            }
            else
            {
                return await
                    this.context.Usuarios.MaxAsync(z => z.IdUsuario) + 1;
            }
        }

        public async Task<Usuario>
            RegisterUserAsync(string nombre, string email
            , string password, string imagen)
        {
            Usuario user = new Usuario();
            user.IdUsuario = await this.GetMaxIdUsuarioAsync();
            user.Nombre = nombre;
            user.Email = email;
            user.Imagen = imagen;
            //CADA USUARIO TENDRA UN SALT DISTINTO
            user.Salt = HelperTools.GenerateSalt();
            //GUARDAMOS EL PASSWORD EN BYTE[]
            user.Password =
                HelperCryptography.EncryptPassword(password, user.Salt);
            user.Activo = false;
            user.TokenMail = HelperTools.GenerateTokenMail();
            this.context.Usuarios.Add(user);
            await this.context.SaveChangesAsync();
            return user;
        }

        public async Task ActivateUserAsync(string token)
        {
            //BUSCAMOS EL USUARIO POR SU TOKEN
            Usuario user = await
                this.context.Usuarios.FirstOrDefaultAsync(x => x.TokenMail == token);
            user.Activo = true;
            user.TokenMail = "";
            await this.context.SaveChangesAsync();
        }

        //NECESITAMOS UN METODO PARA VALIDAR AL USUARIO
        //DICHO METODO DEVOLVERA EL PROPIO USUARIO
        //COMO COMPARAMOS?? email CAMPO UNICO
        //password (12345)
        //1) RECUPERAR EL USER POR SU EMAIL
        //2) RECUPERAMOS EL SALT DEL USUARIO
        //3) CONVERTIMOS DE NUEVO EL PASSWORD CON EL SALT
        //4) RECUPERAMOS EL BYTE[] DE PASSWORD DE LA BBDD
        //5) COMPARAMOS LOS DOS ARRAYS (BBDD) Y EL GENERADO EN EL CODIGO
        public async Task<Usuario> LogInUserAsync(string email, string password)
        {
            Usuario user = await
                this.context.Usuarios.FirstOrDefaultAsync(x => x.Email == email);
            if (user == null)
            {
                return null;
            }
            else
            {
                string salt = user.Salt;
                byte[] temp =
                    HelperCryptography.EncryptPassword(password, salt);
                byte[] passUser = user.Password;
                bool response =
                    HelperTools.CompareArrays(temp, passUser);
                if (response == true)
                {
                    return user;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
