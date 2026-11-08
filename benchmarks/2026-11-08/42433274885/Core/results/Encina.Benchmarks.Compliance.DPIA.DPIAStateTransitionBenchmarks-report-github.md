```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method               | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Approve assessment&#39; | 1,805.9 ns | 21.17 ns | 31.03 ns |  1.00 |    0.02 |    3 | 0.0553 | 0.0267 |     976 B |        1.00 |
| &#39;Reject assessment&#39;  | 1,116.4 ns | 34.59 ns | 50.70 ns |  0.62 |    0.03 |    2 | 0.0210 | 0.0191 |     400 B |        0.41 |
| &#39;Request revision&#39;   | 1,145.2 ns | 34.05 ns | 50.97 ns |  0.63 |    0.03 |    2 | 0.0210 | 0.0191 |     400 B |        0.41 |
| &#39;Expire assessment&#39;  |   915.7 ns | 16.54 ns | 24.76 ns |  0.51 |    0.02 |    1 | 0.0210 | 0.0200 |     368 B |        0.38 |
