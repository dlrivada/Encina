```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                               | Mean       | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|-----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Get assessment by ID (cached)&#39;                      | 1,028.0 ns |   846.7 ns | 46.41 ns |  1.00 |    0.05 |    1 | 0.0038 | 0.0019 |     424 B |        1.00 |
| &#39;Get assessment by request type (pipeline hot-path)&#39; |   940.6 ns | 1,132.8 ns | 62.09 ns |  0.92 |    0.06 |    1 | 0.0038 | 0.0019 |     392 B |        0.92 |
| &#39;Get expired assessments&#39;                            |   987.4 ns |   600.8 ns | 32.93 ns |  0.96 |    0.05 |    1 | 0.0038 | 0.0029 |     400 B |        0.94 |
| &#39;Get all assessments&#39;                                |   945.4 ns |   682.8 ns | 37.42 ns |  0.92 |    0.05 |    1 | 0.0038 | 0.0029 |     400 B |        0.94 |
