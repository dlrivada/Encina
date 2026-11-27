```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.96GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------- |-----------:|-----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Approve assessment&#39; | 1,956.1 ns |   890.0 ns | 48.78 ns |  1.00 |    0.03 |    2 | 0.0095 | 0.0076 |     944 B |        1.00 |
| &#39;Reject assessment&#39;  |   974.8 ns | 1,744.3 ns | 95.61 ns |  0.50 |    0.04 |    1 | 0.0038 | 0.0029 |     384 B |        0.41 |
| &#39;Request revision&#39;   | 1,099.9 ns |   861.4 ns | 47.22 ns |  0.56 |    0.02 |    1 | 0.0038 | 0.0029 |     384 B |        0.41 |
| &#39;Expire assessment&#39;  | 1,000.3 ns | 1,085.7 ns | 59.51 ns |  0.51 |    0.03 |    1 | 0.0038 | 0.0029 |     368 B |        0.39 |
