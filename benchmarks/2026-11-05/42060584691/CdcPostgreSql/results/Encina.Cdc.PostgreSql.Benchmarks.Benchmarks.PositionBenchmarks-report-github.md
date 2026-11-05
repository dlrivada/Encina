```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.021 ns | 0.0176 ns | 0.0010 ns |  0.21 |    0.00 |      - |         - |        0.00 |
| CreatePosition   | 4.832 ns | 1.7890 ns | 0.0981 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 5.939 ns | 3.0290 ns | 0.1660 ns |  1.23 |    0.04 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 5.745 ns | 0.7293 ns | 0.0400 ns |  1.19 |    0.02 | 0.0019 |      32 B |        1.33 |
