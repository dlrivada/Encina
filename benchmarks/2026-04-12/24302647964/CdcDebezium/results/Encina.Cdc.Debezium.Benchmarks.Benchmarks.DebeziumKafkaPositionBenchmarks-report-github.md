```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean         | Error     | StdDev    | Gen0   | Allocated |
|----------------- |-------------:|----------:|----------:|-------:|----------:|
| ComparePositions |     3.468 ns | 0.0052 ns | 0.0073 ns |      - |         - |
| FromBytes        | 2,046.341 ns | 3.6482 ns | 5.4604 ns | 0.0420 |     744 B |
| ToBytes          |   937.473 ns | 3.4235 ns | 4.7993 ns | 0.0153 |     264 B |
