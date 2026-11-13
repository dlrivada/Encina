```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean       | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |-----------:|-----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 1,947.4 ns |   730.2 ns | 40.02 ns |  1.00 |    0.03 |    3 | 0.0134 | 0.0114 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; |   946.6 ns |   260.2 ns | 14.26 ns |  0.49 |    0.01 |    1 | 0.0076 | 0.0057 |     656 B |        0.53 |
| &#39;Policy: get by ID&#39;                            | 1,632.1 ns | 1,708.3 ns | 93.64 ns |  0.84 |    0.04 |    3 | 0.0095 | 0.0076 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 1,384.8 ns |   229.6 ns | 12.58 ns |  0.71 |    0.01 |    2 | 0.0076 | 0.0057 |     680 B |        0.54 |
