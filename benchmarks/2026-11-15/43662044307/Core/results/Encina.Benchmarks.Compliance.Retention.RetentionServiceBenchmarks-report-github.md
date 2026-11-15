```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                         | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 1,989.0 ns | 59.38 ns | 88.87 ns |  1.00 |    0.06 |    4 | 0.0134 | 0.0114 |    1280 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; |   969.3 ns | 24.33 ns | 35.67 ns |  0.49 |    0.03 |    1 | 0.0076 | 0.0067 |     672 B |        0.53 |
| &#39;Policy: get by ID&#39;                            | 1,666.0 ns | 60.70 ns | 90.86 ns |  0.84 |    0.06 |    3 | 0.0095 | 0.0076 |     896 B |        0.70 |
| &#39;Policy: deactivate&#39;                           | 1,325.0 ns | 29.55 ns | 43.32 ns |  0.67 |    0.04 |    2 | 0.0076 | 0.0057 |     712 B |        0.56 |
