CSPROJ = BuildService/BuildService.csproj
STRIP_INCLUDE=s,Include=,,g;s,\",,g;s,\\,/,g
ASSEMBLY = bin/$(shell xpath /Project/PropertyGroup/AssemblyName < $(CSPROJ) 2>/dev/null | sed -r 's,</?AssemblyName>,,g').exe
SOURCES = $(addprefix BuildService/, $(shell xpath /Project/ItemGroup/Compile/@Include < $(CSPROJ) 2>/dev/null | sed '$(STRIP_INCLUDE)'))
REFERENCES = $(addprefix -r:, $(shell xpath /Project/ItemGroup/Reference/@Include < $(CSPROJ) 2>/dev/null | sed '$(STRIP_INCLUDE)'))

$(ASSEMBLY): $(SOURCES)
	mkdir -p $$(dirname $@)
	gmcs -out:$@ $(REFERENCES) $(SOURCES)

clean:
	rm -f $(ASSEMBLY)

run: $(ASSEMBLY)
	@mono $<
