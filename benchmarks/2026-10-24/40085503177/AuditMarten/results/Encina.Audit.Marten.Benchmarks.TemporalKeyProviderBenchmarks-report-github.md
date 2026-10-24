```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |-----------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   129.8 ns |   2.47 ns |  0.14 ns |  1.05 |    0.00 |    1 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |   105.8 ns |  14.63 ns |  0.80 ns |  0.86 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,133.3 ns | 464.90 ns | 25.48 ns | 17.26 |    0.18 |    2 | 0.1450 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 3,564.2 ns | 214.62 ns | 11.76 ns | 28.83 |    0.10 |    3 | 0.0496 | 0.0458 |     848 B |        2.41 |
| GetExistingKey         | 12          |   123.6 ns |   5.35 ns |  0.29 ns |  1.00 |    0.00 |    1 | 0.0210 |      - |     352 B |        1.00 |
