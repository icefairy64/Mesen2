#pragma once
#include "stdafx.h"
#include "NES/Input/Zapper.h"

class NesConsole;

class VsZapper : public Zapper
{
private:
	uint32_t _stateBuffer = 0;

protected:
	void Serialize(Serializer& s) override
	{
		BaseControlDevice::Serialize(s);
		s.Stream(_stateBuffer);
	}

	void RefreshStateBuffer() override
	{
		_stateBuffer = 0x10 | (IsLightFound() ? 0x40 : 0x00) | (IsPressed(Zapper::Buttons::Fire) ? 0x80 : 0x00);
	}

public:
	VsZapper(NesConsole* console, uint8_t port) : Zapper(console, ControllerType::NesZapper, port)
	{
	}

	uint8_t ReadRam(uint16_t addr) override
	{
		if(IsCurrentPort(addr)) {
			StrobeProcessRead();
			uint8_t returnValue = _stateBuffer & 0x01;
			_stateBuffer >>= 1;
			return returnValue;
		}

		return 0;
	}

	void WriteRam(uint16_t addr, uint8_t value) override
	{
		StrobeProcessWrite(value);
	}
};