```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Approve assessment&#39; | 1,884.3 ns | 813.3 ns | 44.58 ns |  1.00 |    0.03 |    2 | 0.0095 | 0.0076 |     944 B |        1.00 |
| &#39;Reject assessment&#39;  | 1,102.4 ns | 842.5 ns | 46.18 ns |  0.59 |    0.02 |    1 | 0.0038 | 0.0029 |     384 B |        0.41 |
| &#39;Request revision&#39;   | 1,056.5 ns | 235.3 ns | 12.90 ns |  0.56 |    0.01 |    1 | 0.0038 | 0.0029 |     384 B |        0.41 |
| &#39;Expire assessment&#39;  |   899.3 ns | 531.5 ns | 29.13 ns |  0.48 |    0.02 |    1 | 0.0038 | 0.0029 |     368 B |        0.39 |
