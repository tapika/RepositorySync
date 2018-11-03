using SharpSvn;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

class Program
{
    SvnClient svn = new SvnClient();
    String[] urls;
    String[] workDir;


    void Main2(String[] args)
    {
        //  from where                                      to where
        urls = new[] { "https://svn.code.sf.net/p/syncproj/code/", "https://github.com/tapika/syncProj.git/trunk"};
        workDir = new[] { @"d:\Prototyping\svnSource", @"d:\Prototyping\svnTarget" };
        Uri[] uris = new Uri[2];

        for( int i=0; i < urls.Length;i++)
        {
            uris[i] = new Uri(urls[i]);

            if (!Directory.Exists(workDir[i]))
                if (i == 0)
                    svn.CheckOut(uris[i], workDir[i], new SvnCheckOutArgs() { Revision = new SvnRevision(i) });
                else
                    svn.CheckOut(uris[i], workDir[i]);
        }

        SvnInfoEventArgs info;
        svn.GetInfo(uris[0],out info);

        SvnLogArgs svnArgs = new SvnLogArgs { Start = 1 /*change next svn revision number*/, End = info.Revision, RetrieveAllProperties = true };
        Collection<SvnLogEventArgs> list;
        svn.GetLog(uris[0], svnArgs, out list);


        foreach (SvnLogEventArgs log in list)
        {
            long rev = log.Revision;
            String msg = log.LogMessage;
            Console.WriteLine("Commit " + rev + ": " + msg);

            svn.Update(workDir[0], new SvnUpdateArgs() { Revision = new SvnRevision(rev) });

            foreach (SvnChangeItem chItem in log.ChangedPaths)
            {
                String path = chItem.Path;

                switch (chItem.Action)
                {
                    case SvnChangeAction.Add:
                        if (chItem.NodeKind == SvnNodeKind.Directory)
                        {
                            try { svn.CreateDirectory(workDir[1] + path); } catch { }
                        }
                        else {
                            File.Copy(workDir[0] + path, workDir[1] + path, true);
                            try { svn.Add(workDir[1] + path); } catch { }
                            
                        }
                        break;

                    case SvnChangeAction.Replace:
                    case SvnChangeAction.Delete:
                        if (chItem.NodeKind == SvnNodeKind.Directory)
                        {
                            svn.Delete(workDir[1] + path);
                        } else
                        {
                            File.Delete(workDir[1] + path);
                            svn.Delete(workDir[1] + path);
                        }
                        break;

                    case SvnChangeAction.Modify:
                        if (chItem.NodeKind == SvnNodeKind.Directory)
                        {
                            Collection<SvnPropertyListEventArgs> propList = null;
                            svn.GetPropertyList(new SvnPathTarget(workDir[0] + path), out propList);

                            foreach (SvnPropertyListEventArgs p in propList)
                                foreach (SvnPropertyValue pv in p.Properties)
                                    svn.SetProperty(workDir[1] + path, pv.Key, pv.StringValue);
                        }
                        else
                        {
                            File.Copy(workDir[0] + path, workDir[1] + path, true);
                        }
                        break;
                }
            }
            svn.Commit(workDir[1], new SvnCommitArgs() { LogMessage = msg });
        }
    }


    static void Main(string[] args)
    {
        try
        {
            new Program().Main2(args);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}


