```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.25GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------- |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Approve assessment&#39; | 1,620.1 ns |   606.7 ns |  33.26 ns |  1.00 |    0.03 |    2 | 0.0095 | 0.0076 |     944 B |        1.00 |
| &#39;Reject assessment&#39;  |   977.6 ns | 2,642.7 ns | 144.86 ns |  0.60 |    0.08 |    1 | 0.0038 | 0.0029 |     384 B |        0.41 |
| &#39;Request revision&#39;   |   847.1 ns | 1,875.5 ns | 102.80 ns |  0.52 |    0.06 |    1 | 0.0038 | 0.0029 |     384 B |        0.41 |
| &#39;Expire assessment&#39;  |   880.3 ns |   744.5 ns |  40.81 ns |  0.54 |    0.02 |    1 | 0.0038 | 0.0029 |     368 B |        0.39 |
