
using System.IO;

namespace ruinvault;

class PathUtil {
	public delegate FnParts ModPartsFn(FnParts parts);
	
	public struct FnParts {
		public string path;
		public string name;
		public string ext; // includes dot
		public string Combine() {
			return Path.Combine(this.path, $"{this.name}{this.ext}");
		}
	}
	
	public static FnParts Explode(string original) {
		return new FnParts{
			path= new FileInfo(original).Directory.FullName,
			name = Path.GetFileNameWithoutExtension(original),
			ext = Path.GetExtension(original)??""
		};
	}
	
	public static string ReplaceFilename(string original, string newfilename) {
		var p = PathUtil.Explode(original);
		p.name = Path.GetFileNameWithoutExtension(newfilename);
		p.ext = Path.GetExtension(newfilename)??"";
		return p.Combine();
	}

	public static string ReplaceBasename(string original, string newfilename) {
		var p = PathUtil.Explode(original);
		p.name = Path.GetFileNameWithoutExtension(newfilename);
		return p.Combine();
	}
	
	public static string PrefixBasename(string fp, string pf) {
			return PathUtil.ModParts(fp, (p) => {p.name=$"{pf}{p.name}"; return p;});
	}

	public static string PostfixBasename(string fp, string pf) {
			return PathUtil.ModParts(fp, (p) => {p.name=$"{p.name}{pf}"; return p;});
	}
	
	public static string ModParts(string original, ModPartsFn modfn) {
		var p = PathUtil.Explode(original);
		return modfn(p).Combine();
	}
}
