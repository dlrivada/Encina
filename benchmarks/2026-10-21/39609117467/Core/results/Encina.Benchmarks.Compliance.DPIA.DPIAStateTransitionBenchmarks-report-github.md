```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.58GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------- |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Approve assessment&#39; | 2,130.1 ns |   377.9 ns |  20.71 ns |  1.00 |    0.01 |    2 | 0.0095 | 0.0076 |     944 B |        1.00 |
| &#39;Reject assessment&#39;  | 1,060.4 ns | 2,394.7 ns | 131.26 ns |  0.50 |    0.05 |    1 | 0.0038 | 0.0019 |     368 B |        0.39 |
| &#39;Request revision&#39;   | 1,036.5 ns |   313.6 ns |  17.19 ns |  0.49 |    0.01 |    1 | 0.0038 | 0.0019 |     368 B |        0.39 |
| &#39;Expire assessment&#39;  |   995.9 ns | 2,614.3 ns | 143.30 ns |  0.47 |    0.06 |    1 | 0.0038 | 0.0019 |     352 B |        0.37 |
