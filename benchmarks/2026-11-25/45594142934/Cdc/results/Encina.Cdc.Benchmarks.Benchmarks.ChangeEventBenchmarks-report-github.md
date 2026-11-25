```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 22.620 ns | 12.9479 ns | 0.7097 ns |  0.63 |    0.02 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.400 ns |  0.2502 ns | 0.0137 ns |  0.04 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 12.768 ns |  5.2026 ns | 0.2852 ns |  0.36 |    0.01 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 35.803 ns |  4.3714 ns | 0.2396 ns |  1.00 |    0.01 | 0.0033 |      56 B |        1.00 |
