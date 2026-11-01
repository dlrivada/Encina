```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                 | PeriodCount | Mean        | Error     | StdDev     | Median      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |------------:|----------:|-----------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   133.25 ns |  1.049 ns |   1.537 ns |   133.21 ns |  1.13 |    0.04 |    3 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |    90.31 ns |  3.871 ns |   5.674 ns |    85.57 ns |  0.76 |    0.05 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,228.61 ns |  9.563 ns |  13.406 ns | 2,232.11 ns | 18.87 |    0.57 |    4 | 0.1450 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 4,175.05 ns | 87.688 ns | 125.760 ns | 4,155.24 ns | 35.35 |    1.48 |    5 | 0.0496 | 0.0458 |     848 B |        2.41 |
| GetExistingKey         | 12          |   118.20 ns |  2.470 ns |   3.542 ns |   120.23 ns |  1.00 |    0.04 |    2 | 0.0210 |      - |     352 B |        1.00 |
