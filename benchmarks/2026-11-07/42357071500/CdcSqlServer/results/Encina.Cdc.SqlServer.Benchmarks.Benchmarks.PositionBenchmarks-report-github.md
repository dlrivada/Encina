```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.058 ns | 0.0392 ns | 0.0021 ns |  0.19 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 5.539 ns | 4.4878 ns | 0.2460 ns |  1.00 |    0.06 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 6.083 ns | 3.8360 ns | 0.2103 ns |  1.10 |    0.05 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 5.807 ns | 4.0614 ns | 0.2226 ns |  1.05 |    0.05 | 0.0019 |      32 B |        1.33 |
