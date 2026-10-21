```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean     | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |---------:|-----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Get assessment by ID (cached)&#39;                      | 988.5 ns |   983.8 ns | 53.93 ns |  1.00 |    0.07 |    1 | 0.0038 | 0.0019 |     424 B |        1.00 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; | 936.1 ns | 1,214.8 ns | 66.59 ns |  0.95 |    0.07 |    1 | 0.0038 | 0.0019 |     392 B |        0.92 |
| &#39;Get expired assessments&#39;                            | 989.8 ns |   799.1 ns | 43.80 ns |  1.00 |    0.06 |    1 | 0.0038 | 0.0029 |     400 B |        0.94 |
| &#39;Get all assessments&#39;                                | 946.4 ns |   762.5 ns | 41.80 ns |  0.96 |    0.06 |    1 | 0.0038 | 0.0029 |     400 B |        0.94 |
