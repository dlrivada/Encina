```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 25.375 ns | 46.1238 ns | 2.5282 ns |  0.80 |    0.07 | 0.0016 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.424 ns |  0.5126 ns | 0.0281 ns |  0.04 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 11.668 ns |  3.8951 ns | 0.2135 ns |  0.37 |    0.01 | 0.0007 |      56 B |        1.00 |
| CreateChangeMetadata       | 31.816 ns |  2.3097 ns | 0.1266 ns |  1.00 |    0.00 | 0.0007 |      56 B |        1.00 |
