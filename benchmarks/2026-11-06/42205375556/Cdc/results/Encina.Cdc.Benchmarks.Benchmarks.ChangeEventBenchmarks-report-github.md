```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.79GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 22.454 ns | 12.5121 ns | 0.6858 ns |  0.63 |    0.02 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.386 ns |  0.0305 ns | 0.0017 ns |  0.04 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 13.093 ns |  1.6878 ns | 0.0925 ns |  0.37 |    0.00 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 35.644 ns |  5.0641 ns | 0.2776 ns |  1.00 |    0.01 | 0.0033 |      56 B |        1.00 |
