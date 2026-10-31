```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.58GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Get assessment by ID (cached)&#39;                      | 1,143.4 ns | 392.6 ns | 21.52 ns |  1.00 |    0.02 |    1 | 0.0248 | 0.0229 |     424 B |        1.00 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; |   992.4 ns | 476.6 ns | 26.12 ns |  0.87 |    0.02 |    1 | 0.0229 | 0.0210 |     392 B |        0.92 |
| &#39;Get expired assessments&#39;                            |   944.2 ns | 314.6 ns | 17.24 ns |  0.83 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        0.94 |
| &#39;Get all assessments&#39;                                |   887.9 ns | 404.5 ns | 22.17 ns |  0.78 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        0.94 |
