```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.31GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 1,804.8 ns | 582.4 ns | 31.92 ns |  1.00 |    0.02 |    2 | 0.0744 | 0.0362 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; |   606.2 ns | 314.1 ns | 17.22 ns |  0.34 |    0.01 |    1 | 0.0391 | 0.0191 |     672 B |        0.54 |
| &#39;Policy: get by ID&#39;                            | 1,577.0 ns | 588.0 ns | 32.23 ns |  0.87 |    0.02 |    2 | 0.0515 | 0.0248 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 1,371.3 ns | 131.7 ns |  7.22 ns |  0.76 |    0.01 |    2 | 0.0401 | 0.0191 |     680 B |        0.54 |
