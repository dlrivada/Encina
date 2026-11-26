```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean     | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Get assessment by ID (cached)&#39;                      | 851.5 ns | 177.3 ns |  9.72 ns |  1.00 |    0.01 |    1 | 0.0248 | 0.0229 |     424 B |        1.00 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; | 791.9 ns | 227.2 ns | 12.45 ns |  0.93 |    0.02 |    1 | 0.0229 | 0.0219 |     408 B |        0.96 |
| &#39;Get expired assessments&#39;                            | 719.6 ns | 168.5 ns |  9.24 ns |  0.85 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        0.94 |
| &#39;Get all assessments&#39;                                | 671.3 ns | 144.4 ns |  7.91 ns |  0.79 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        0.94 |
