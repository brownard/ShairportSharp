using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arm7
{
    static class Cmd
    {
        public const int rebase_size = 608;
        public const int rebase_off = 2093056;

        public const int bind_size = 4528;
        public const int bind_off = 2093664;

        public const int weak_bind_size = 40;
        public const int weak_bind_off = 2098192;

        public const int lazy_bind_size = 10892;
        public const int lazy_bind_off = 2098232;

        public const int export_size = 328;
        public const int export_off = 2109124;
    }
}
