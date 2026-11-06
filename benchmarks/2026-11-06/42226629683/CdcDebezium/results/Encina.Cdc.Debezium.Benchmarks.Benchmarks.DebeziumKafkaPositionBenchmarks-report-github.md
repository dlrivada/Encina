```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean         | Error      | StdDev     | Gen0   | Allocated |
|----------------- |-------------:|-----------:|-----------:|-------:|----------:|
| ComparePositions |     2.740 ns |   1.488 ns |  0.0816 ns |      - |         - |
| FromBytes        | 1,476.576 ns | 188.178 ns | 10.3147 ns | 0.0439 |     744 B |
| ToBytes          |   674.657 ns |  45.899 ns |  2.5159 ns | 0.0153 |     264 B |
