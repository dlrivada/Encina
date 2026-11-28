```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.82GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |-----------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   153.1 ns |  24.04 ns |  1.32 ns |  1.19 |    0.03 |    1 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |   105.6 ns |  38.68 ns |  2.12 ns |  0.82 |    0.03 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,558.6 ns | 644.45 ns | 35.32 ns | 19.97 |    0.60 |    2 | 0.1450 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 3,727.9 ns | 311.16 ns | 17.06 ns | 29.09 |    0.81 |    3 | 0.0496 | 0.0458 |     848 B |        2.41 |
| GetExistingKey         | 12          |   128.2 ns |  75.17 ns |  4.12 ns |  1.00 |    0.04 |    1 | 0.0210 |      - |     352 B |        1.00 |
