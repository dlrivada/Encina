```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.92GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 22.109 ns | 13.2018 ns | 0.7236 ns |  0.65 |    0.02 | 0.0016 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.448 ns |  0.0301 ns | 0.0016 ns |  0.04 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 10.128 ns |  2.4013 ns | 0.1316 ns |  0.30 |    0.00 | 0.0007 |      56 B |        1.00 |
| CreateChangeMetadata       | 33.856 ns |  1.0892 ns | 0.0597 ns |  1.00 |    0.00 | 0.0007 |      56 B |        1.00 |
