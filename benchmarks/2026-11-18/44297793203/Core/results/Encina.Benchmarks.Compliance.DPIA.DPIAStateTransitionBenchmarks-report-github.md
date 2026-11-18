```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------- |-----------:|-----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Approve assessment&#39; | 1,912.8 ns | 1,627.4 ns | 89.20 ns |  1.00 |    0.06 |    2 | 0.0534 | 0.0267 |     944 B |        1.00 |
| &#39;Reject assessment&#39;  | 1,035.2 ns |   564.1 ns | 30.92 ns |  0.54 |    0.03 |    1 | 0.0210 | 0.0191 |     368 B |        0.39 |
| &#39;Request revision&#39;   |   981.6 ns |   638.6 ns | 35.00 ns |  0.51 |    0.03 |    1 | 0.0210 | 0.0191 |     368 B |        0.39 |
| &#39;Expire assessment&#39;  |   889.8 ns |   385.0 ns | 21.10 ns |  0.47 |    0.02 |    1 | 0.0210 | 0.0200 |     368 B |        0.39 |
