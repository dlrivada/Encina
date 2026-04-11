```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean         | Error      | StdDev     | Median       | Gen0   | Allocated |
|----------------- |-------------:|-----------:|-----------:|-------------:|-------:|----------:|
| ComparePositions |     3.522 ns |  0.0437 ns |  0.0598 ns |     3.484 ns |      - |         - |
| FromBytes        | 2,073.881 ns |  2.9294 ns |  4.2013 ns | 2,074.297 ns | 0.0420 |     744 B |
| ToBytes          |   963.625 ns | 20.8807 ns | 30.6066 ns |   987.774 ns | 0.0153 |     264 B |
