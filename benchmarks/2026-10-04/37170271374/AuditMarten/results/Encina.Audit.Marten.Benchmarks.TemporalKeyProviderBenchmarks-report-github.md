```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                 | PeriodCount | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |-----------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   167.5 ns |   2.87 ns |   4.21 ns |  1.19 |    0.03 |    3 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |   112.8 ns |   0.56 ns |   0.83 ns |  0.80 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,756.0 ns |  13.88 ns |  20.77 ns | 19.51 |    0.33 |    4 | 0.1450 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 4,331.8 ns | 100.54 ns | 144.19 ns | 30.66 |    1.11 |    5 | 0.0496 | 0.0458 |     848 B |        2.41 |
| GetExistingKey         | 12          |   141.3 ns |   1.51 ns |   2.22 ns |  1.00 |    0.02 |    2 | 0.0210 |      - |     352 B |        1.00 |
