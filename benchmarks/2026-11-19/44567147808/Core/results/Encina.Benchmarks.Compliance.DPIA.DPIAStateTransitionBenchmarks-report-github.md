```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------- |-----------:|-----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Approve assessment&#39; | 1,745.3 ns | 1,640.0 ns | 89.89 ns |  1.00 |    0.06 |    2 | 0.0095 | 0.0076 |     944 B |        1.00 |
| &#39;Reject assessment&#39;  | 1,002.7 ns |   440.3 ns | 24.13 ns |  0.58 |    0.03 |    1 | 0.0038 | 0.0029 |     384 B |        0.41 |
| &#39;Request revision&#39;   |   996.5 ns |   778.9 ns | 42.70 ns |  0.57 |    0.03 |    1 | 0.0038 | 0.0029 |     384 B |        0.41 |
| &#39;Expire assessment&#39;  |   964.0 ns |   825.5 ns | 45.25 ns |  0.55 |    0.03 |    1 | 0.0038 | 0.0029 |     368 B |        0.39 |
