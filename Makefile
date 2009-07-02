CSPROJ = BuildService/BuildService.csproj
STRIP_INCLUDE=s,Include=,,g;s,\",,g;s,\\,/,g
ASSEMBLY = bin/$(shell xpath /Project/PropertyGroup/AssemblyName < $(CSPROJ) 2>/dev/null | sed -r 's,</?AssemblyName>,,g').exe
SOURCES = $(addprefix BuildService/, $(shell xpath /Project/ItemGroup/Compile/@Include < $(CSPROJ) 2>/dev/null | sed '$(STRIP_INCLUDE)'))
REFERENCES = $(addprefix -r:, $(shell xpath /Project/ItemGroup/Reference/@Include < $(CSPROJ) 2>/dev/null | sed '$(STRIP_INCLUDE)'))

PREFIX :=
DATADIR := $(PREFIX)/share
BINDIR := $(PREFIX)/bin
PKGDATADIR := $(DATADIR)/obs-tools

$(ASSEMBLY): $(SOURCES)
	mkdir -p $$(dirname $@)
	gmcs -debug -out:$@ -nowarn:0618 $(REFERENCES) $(SOURCES)

clean:
	rm -f obs-factory-status
	rm -f $(ASSEMBLY){,.mdb}

run: $(ASSEMBLY)
	@mono --debug $< Moblin:UI Moblin:Base Moblin:Factory

configure:
	@if [ -z "$(PREFIX)" ]; then \
		echo "You must set PREFIX before installing/uninstalling:"; \
		echo; \
		echo "   $$ sudo make install PREFIX=/usr"; \
		echo; \
		exit 1; \
	fi;

obs-factory-status: obs-factory-status.in
	sed 's,@PKGDATADIR@,$(PKGDATADIR),g' < $< > $@

install: configure obs-factory-status $(ASSEMBLY)
	mkdir -p $(BINDIR)
	install -m 0755 obs-factory-status $(BINDIR)
	mkdir -p $(PKGDATADIR)
	install -m 0644 $(ASSEMBLY) $(PKGDATADIR)
	install -m 0644 $(ASSEMBLY).mdb $(PKGDATADIR)

uninstall: configure
	rm -f $(BINDIR)/obs-factory-status
	rm -rf $(PKGDATADIR)
