```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.48GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean         | Error       | StdDev    | Gen0   | Allocated |
|----------------- |-------------:|------------:|----------:|-------:|----------:|
| ComparePositions |     3.087 ns |   0.7107 ns | 0.0390 ns |      - |         - |
| FromBytes        | 1,914.993 ns | 115.2194 ns | 6.3156 ns | 0.0076 |     744 B |
| ToBytes          |   730.874 ns |   6.1828 ns | 0.3389 ns | 0.0029 |     264 B |
