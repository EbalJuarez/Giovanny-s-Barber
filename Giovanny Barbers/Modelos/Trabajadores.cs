using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Giovanny_Barbers.Modelos
{
    public class Trabajador
    {
        public int IdTrabajador { get; set; }
        public int? IdUsuario { get; set; }  // el ? permite que sea null
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string Telefono { get; set; }
        public string Direccion { get; set; }
    }
}
