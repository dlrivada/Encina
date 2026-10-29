```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Get assessment by ID (cached)&#39;                      | 1,121.7 ns | 326.0 ns | 17.87 ns |  1.00 |    0.02 |    1 | 0.0248 | 0.0229 |     424 B |        1.00 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; | 1,022.7 ns | 702.9 ns | 38.53 ns |  0.91 |    0.03 |    1 | 0.0229 | 0.0210 |     392 B |        0.92 |
| &#39;Get expired assessments&#39;                            |   991.4 ns | 183.1 ns | 10.03 ns |  0.88 |    0.01 |    1 | 0.0229 | 0.0210 |     384 B |        0.91 |
| &#39;Get all assessments&#39;                                |   896.2 ns | 377.8 ns | 20.71 ns |  0.80 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        0.94 |
