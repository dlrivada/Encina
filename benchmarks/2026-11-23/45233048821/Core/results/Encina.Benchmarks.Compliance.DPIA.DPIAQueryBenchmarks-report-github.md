```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Get assessment by ID (cached)&#39;                      | 1,127.3 ns | 512.1 ns | 28.07 ns |  1.00 |    0.03 |    1 | 0.0248 | 0.0229 |     424 B |        1.00 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; | 1,001.5 ns | 284.0 ns | 15.57 ns |  0.89 |    0.02 |    1 | 0.0229 | 0.0210 |     392 B |        0.92 |
| &#39;Get expired assessments&#39;                            |   950.1 ns | 554.6 ns | 30.40 ns |  0.84 |    0.03 |    1 | 0.0229 | 0.0210 |     384 B |        0.91 |
| &#39;Get all assessments&#39;                                |   903.5 ns | 330.5 ns | 18.12 ns |  0.80 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        0.94 |
