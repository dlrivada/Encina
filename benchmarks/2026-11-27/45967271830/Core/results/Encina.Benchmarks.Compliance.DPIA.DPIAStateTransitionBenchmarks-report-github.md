```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.04GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------- |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Approve assessment&#39; | 1,933.8 ns |   158.5 ns |   8.69 ns |  1.00 |    0.01 |    2 | 0.0095 | 0.0076 |     944 B |        1.00 |
| &#39;Reject assessment&#39;  | 1,122.3 ns |   469.8 ns |  25.75 ns |  0.58 |    0.01 |    1 | 0.0038 | 0.0029 |     384 B |        0.41 |
| &#39;Request revision&#39;   |   976.6 ns | 2,118.3 ns | 116.11 ns |  0.51 |    0.05 |    1 | 0.0038 | 0.0029 |     384 B |        0.41 |
| &#39;Expire assessment&#39;  |   849.3 ns | 1,632.5 ns |  89.48 ns |  0.44 |    0.04 |    1 | 0.0038 | 0.0029 |     368 B |        0.39 |
