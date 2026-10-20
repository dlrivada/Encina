```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Get assessment by ID (cached)&#39;                      | 1,179.9 ns | 142.4 ns |  7.81 ns |  1.00 |    0.01 |    1 | 0.0248 | 0.0229 |     424 B |        1.00 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; | 1,016.6 ns | 808.7 ns | 44.33 ns |  0.86 |    0.03 |    1 | 0.0229 | 0.0210 |     392 B |        0.92 |
| &#39;Get expired assessments&#39;                            |   901.5 ns | 131.7 ns |  7.22 ns |  0.76 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        0.94 |
| &#39;Get all assessments&#39;                                |   872.6 ns | 327.8 ns | 17.97 ns |  0.74 |    0.01 |    1 | 0.0229 | 0.0219 |     400 B |        0.94 |
