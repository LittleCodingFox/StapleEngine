#include <stdio.h>

#include "App/App.hpp"
using namespace Staple;

int main()
{
	AppSettings settings;

	AppPlayer player(settings);

	player.Run();

	return 0;
}
