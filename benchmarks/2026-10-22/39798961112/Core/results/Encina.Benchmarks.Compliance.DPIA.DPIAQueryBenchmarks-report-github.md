```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                               | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Get assessment by ID (cached)&#39;                      | 1,150.6 ns | 23.03 ns | 34.47 ns |  1.00 |    0.04 |    3 | 0.0248 | 0.0229 |     456 B |        1.00 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; | 1,002.6 ns | 26.99 ns | 40.40 ns |  0.87 |    0.04 |    2 | 0.0229 | 0.0210 |     424 B |        0.93 |
| &#39;Get expired assessments&#39;                            |   982.2 ns | 17.79 ns | 26.63 ns |  0.85 |    0.03 |    2 | 0.0229 | 0.0219 |     400 B |        0.88 |
| &#39;Get all assessments&#39;                                |   928.7 ns |  6.12 ns |  9.15 ns |  0.81 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        0.88 |
