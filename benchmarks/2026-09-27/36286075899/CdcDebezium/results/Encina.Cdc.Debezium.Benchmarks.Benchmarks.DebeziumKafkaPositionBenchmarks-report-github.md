```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.57GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean         | Error     | StdDev     | Gen0   | Allocated |
|----------------- |-------------:|----------:|-----------:|-------:|----------:|
| ComparePositions |     2.517 ns | 0.0010 ns |  0.0014 ns |      - |         - |
| FromBytes        | 1,707.979 ns | 8.9339 ns | 12.2288 ns | 0.0076 |     744 B |
| ToBytes          |   659.481 ns | 5.4700 ns |  8.0178 ns | 0.0029 |     264 B |
