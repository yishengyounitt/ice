// **********************************************************************
//
// Copyright (c) 2001
// MutableRealms, Inc.
// Huntsville, AL, USA
//
// All Rights Reserved
//
// **********************************************************************

class LibraryServer extends Freeze.Application
{
    public int
    runFreeze(String[] args, Freeze.DBEnvironment dbEnv)
    {
	Ice.Properties properties = communicator().getProperties();
    
	Freeze.DB dbBooks = dbEnv.openDB("books", true);
	Freeze.DB dbAuthors = dbEnv.openDB("authors", true);
    
	//
	// Create an Evictor for books.
	//
	Freeze.EvictorPersistenceMode mode;
	int v = 0;
        try
        {
            v = Integer.parseInt(properties.getProperty("Library.SaveAfterMutatingOperation"));
        }
        catch (NumberFormatException ex)
        {
	}

	if (v != 0)
	{
	    mode = Freeze.EvictorPersistenceMode.SaveAfterMutatingOperation;
	}
	else
	{
	    mode = Freeze.EvictorPersistenceMode.SaveUponEviction;
	}
	Freeze.Evictor evictor = dbBooks.createEvictor(mode);

	v = 0;
        try
        {
            v = Integer.parseInt(properties.getProperty("Library.EvictorSize"));
        }
        catch (NumberFormatException ex)
        {
        }

	if (v != 0)
	{
	    evictor.setSize(v);
	}
    
	//
	// Create an Object Adapter, use the Evictor as Servant
	// Locator.
	//
	Ice.ObjectAdapter adapter = communicator().createObjectAdapter("LibraryAdapter");
	adapter.addServantLocator(evictor, "book");
    
	//
	// Create the library, and add it to the Object Adapter.
	//
	LibraryI library = new LibraryI(adapter, dbAuthors, evictor);
	adapter.add(library, Ice.Util.stringToIdentity("library"));
    
	//
	// Create and install a factory and initializer for books.
	//
	Ice.ObjectFactory bookFactory = new BookFactory(library);
	communicator().addObjectFactory(bookFactory, "::Book");
    
	//
	// Everything ok, let's go.
	//
	adapter.activate();

	//
	// TODO:
	//
	//shutdownOnInterrupt();

	communicator().waitForShutdown();

	//ignoreInterrupt();

	return 0;
    }

    LibraryServer(String dbEnvName)
    {
	super(dbEnvName);
    }
}

public class Server
{
    static public void
    main(String[] args)
    {
	LibraryServer app = new LibraryServer("db");
	app.main("test.Freeze.library.Server", args, "config");
    }
}
