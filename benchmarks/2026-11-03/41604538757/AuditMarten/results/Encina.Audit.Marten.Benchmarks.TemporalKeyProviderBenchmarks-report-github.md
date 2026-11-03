```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean       | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |-----------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| GetOrCreateExistingKey | 12          |   163.0 ns |     3.40 ns |  0.19 ns |  1.19 |    0.00 |    1 | 0.0196 |      - |     328 B |        0.93 |
| IsKeyDestroyed         | 12          |   116.3 ns |     1.51 ns |  0.08 ns |  0.85 |    0.00 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          | 2,923.9 ns |    85.51 ns |  4.69 ns | 21.33 |    0.06 |    2 | 0.1450 |      - |    2464 B |        7.00 |
| CreateNewKey           | 12          | 3,893.4 ns | 1,394.97 ns | 76.46 ns | 28.40 |    0.49 |    3 | 0.0496 | 0.0458 |     848 B |        2.41 |
| GetExistingKey         | 12          |   137.1 ns |     6.43 ns |  0.35 ns |  1.00 |    0.00 |    1 | 0.0210 |      - |     352 B |        1.00 |
