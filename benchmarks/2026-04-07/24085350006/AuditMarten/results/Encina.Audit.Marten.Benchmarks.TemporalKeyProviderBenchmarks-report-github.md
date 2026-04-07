```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |-----------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   157.2 ns |  22.91 ns |  1.26 ns |  1.14 |    0.01 |    2 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |   112.1 ns |  15.62 ns |  0.86 ns |  0.81 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,786.9 ns | 617.08 ns | 33.82 ns | 20.24 |    0.25 |    3 | 0.1450 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 3,886.7 ns | 831.10 ns | 45.56 ns | 28.23 |    0.34 |    4 | 0.0458 | 0.0381 |     848 B |        2.41 |
| GetExistingKey         | 12          |   137.7 ns |  19.58 ns |  1.07 ns |  1.00 |    0.01 |    2 | 0.0210 |      - |     352 B |        1.00 |
