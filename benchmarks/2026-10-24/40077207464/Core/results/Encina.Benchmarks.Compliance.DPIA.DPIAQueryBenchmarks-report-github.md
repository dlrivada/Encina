```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean     | Error     | StdDev   | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |---------:|----------:|---------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Get assessment by ID (cached)&#39;                      | 966.3 ns |  18.77 ns |  1.03 ns |  1.00 |    1 | 0.0248 | 0.0238 |     440 B |        1.00 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; | 835.4 ns | 160.73 ns |  8.81 ns |  0.86 |    1 | 0.0229 | 0.0219 |     408 B |        0.93 |
| &#39;Get expired assessments&#39;                            | 763.9 ns | 149.39 ns |  8.19 ns |  0.79 |    1 | 0.0229 | 0.0219 |     400 B |        0.91 |
| &#39;Get all assessments&#39;                                | 729.2 ns | 299.41 ns | 16.41 ns |  0.75 |    1 | 0.0229 | 0.0219 |     400 B |        0.91 |
