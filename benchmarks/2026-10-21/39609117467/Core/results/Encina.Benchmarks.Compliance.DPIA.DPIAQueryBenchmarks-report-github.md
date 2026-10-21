```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean     | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Get assessment by ID (cached)&#39;                      | 915.6 ns | 439.3 ns | 24.08 ns |  1.00 |    0.03 |    1 | 0.0248 | 0.0238 |     440 B |        1.00 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; | 797.6 ns | 358.4 ns | 19.65 ns |  0.87 |    0.03 |    1 | 0.0229 | 0.0219 |     408 B |        0.93 |
| &#39;Get expired assessments&#39;                            | 759.5 ns | 367.9 ns | 20.17 ns |  0.83 |    0.03 |    1 | 0.0229 | 0.0219 |     400 B |        0.91 |
| &#39;Get all assessments&#39;                                | 753.2 ns | 226.8 ns | 12.43 ns |  0.82 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        0.91 |
