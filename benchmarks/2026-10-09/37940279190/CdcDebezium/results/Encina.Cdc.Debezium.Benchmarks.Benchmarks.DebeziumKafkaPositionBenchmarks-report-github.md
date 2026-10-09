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
| ComparePositions |     3.501 ns |   0.0438 ns |  0.0024 ns |      - |         - |
| FromBytes        | 1,931.704 ns | 247.1039 ns | 13.5446 ns | 0.0420 |     744 B |
| ToBytes          |   862.535 ns | 154.5939 ns |  8.4738 ns | 0.0153 |     264 B |
