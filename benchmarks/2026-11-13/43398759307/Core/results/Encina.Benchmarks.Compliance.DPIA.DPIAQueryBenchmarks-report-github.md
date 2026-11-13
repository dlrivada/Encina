```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.99GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Get assessment by ID (cached)&#39;                      | 1,085.8 ns | 258.1 ns | 14.15 ns |  1.00 |    0.02 |    1 | 0.0248 | 0.0229 |     424 B |        1.00 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; |   971.8 ns | 143.3 ns |  7.86 ns |  0.90 |    0.01 |    1 | 0.0229 | 0.0210 |     392 B |        0.92 |
| &#39;Get expired assessments&#39;                            |   906.3 ns | 258.3 ns | 14.16 ns |  0.83 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        0.94 |
| &#39;Get all assessments&#39;                                |   860.4 ns | 273.2 ns | 14.97 ns |  0.79 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        0.94 |
