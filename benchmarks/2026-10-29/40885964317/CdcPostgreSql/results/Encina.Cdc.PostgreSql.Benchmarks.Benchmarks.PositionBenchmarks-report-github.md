```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean     | Error    | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|---------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 1.415 ns | 1.625 ns | 0.0891 ns |  0.23 |    0.01 |      - |         - |        0.00 |
| CreatePosition   | 6.225 ns | 3.345 ns | 0.1834 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 8.103 ns | 7.956 ns | 0.4361 ns |  1.30 |    0.07 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 7.275 ns | 3.557 ns | 0.1950 ns |  1.17 |    0.04 | 0.0019 |      32 B |        1.33 |
