```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean         | Error       | StdDev     | Gen0   | Allocated |
|----------------- |-------------:|------------:|-----------:|-------:|----------:|
| ComparePositions |     3.521 ns |   0.4931 ns |  0.0270 ns |      - |         - |
| FromBytes        | 1,922.554 ns | 188.3839 ns | 10.3260 ns | 0.0420 |     744 B |
| ToBytes          |   879.732 ns |  28.2529 ns |  1.5486 ns | 0.0153 |     264 B |
