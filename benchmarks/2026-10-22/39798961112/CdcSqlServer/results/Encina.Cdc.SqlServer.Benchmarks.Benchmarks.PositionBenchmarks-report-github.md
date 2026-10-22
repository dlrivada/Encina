```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ComparePositions | 0.5804 ns | 0.0653 ns | 0.0956 ns | 0.5156 ns |  0.16 |    0.03 |      - |         - |        0.00 |
| CreatePosition   | 3.5549 ns | 0.1026 ns | 0.1504 ns | 3.5981 ns |  1.00 |    0.06 | 0.0014 |      24 B |        1.00 |
| FromBytes        | 3.8558 ns | 0.0770 ns | 0.1129 ns | 3.8829 ns |  1.09 |    0.06 | 0.0014 |      24 B |        1.00 |
| ToBytes          | 3.5561 ns | 0.1028 ns | 0.1507 ns | 3.5373 ns |  1.00 |    0.06 | 0.0019 |      32 B |        1.33 |
