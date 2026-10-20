```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.277 ns | 0.0445 ns | 0.0024 ns |  0.15 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 8.359 ns | 9.7347 ns | 0.5336 ns |  1.00 |    0.08 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 7.793 ns | 0.3305 ns | 0.0181 ns |  0.93 |    0.05 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 6.849 ns | 1.0940 ns | 0.0600 ns |  0.82 |    0.05 | 0.0019 |      32 B |        1.33 |
