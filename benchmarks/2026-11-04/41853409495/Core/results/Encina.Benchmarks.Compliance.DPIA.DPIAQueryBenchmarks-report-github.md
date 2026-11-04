```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean     | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |---------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Get assessment by ID (cached)&#39;                      | 938.3 ns | 617.23 ns | 33.83 ns |  1.00 |    0.04 |    1 | 0.0048 | 0.0038 |     440 B |        1.00 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; | 804.5 ns |  48.12 ns |  2.64 ns |  0.86 |    0.03 |    1 | 0.0038 | 0.0029 |     408 B |        0.93 |
| &#39;Get expired assessments&#39;                            | 769.5 ns | 191.16 ns | 10.48 ns |  0.82 |    0.03 |    1 | 0.0038 | 0.0029 |     400 B |        0.91 |
| &#39;Get all assessments&#39;                                | 754.0 ns | 436.70 ns | 23.94 ns |  0.80 |    0.03 |    1 | 0.0038 | 0.0029 |     400 B |        0.91 |
