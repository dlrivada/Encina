```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean         | Error     | StdDev     | Gen0   | Allocated |
|----------------- |-------------:|----------:|-----------:|-------:|----------:|
| ComparePositions |     3.465 ns | 0.0026 ns |  0.0038 ns |      - |         - |
| FromBytes        | 2,017.363 ns | 7.5331 ns | 11.0419 ns | 0.0420 |     744 B |
| ToBytes          |   946.622 ns | 1.4256 ns |  1.9514 ns | 0.0153 |     264 B |
