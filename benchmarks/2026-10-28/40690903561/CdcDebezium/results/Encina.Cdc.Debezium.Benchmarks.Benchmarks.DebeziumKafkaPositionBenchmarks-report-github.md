```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.74GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean         | Error       | StdDev     | Gen0   | Allocated |
|----------------- |-------------:|------------:|-----------:|-------:|----------:|
| ComparePositions |     3.537 ns |   0.1005 ns |  0.0055 ns |      - |         - |
| FromBytes        | 2,064.186 ns | 275.8124 ns | 15.1182 ns | 0.0420 |     744 B |
| ToBytes          |   943.281 ns |  23.4204 ns |  1.2838 ns | 0.0153 |     264 B |
