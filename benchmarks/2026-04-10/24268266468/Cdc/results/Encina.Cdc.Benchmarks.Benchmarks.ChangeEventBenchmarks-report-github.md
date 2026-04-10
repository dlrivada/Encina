```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 25.885 ns | 11.5840 ns | 0.6350 ns |  0.89 |    0.02 | 0.0054 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.470 ns |  0.7793 ns | 0.0427 ns |  0.05 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 13.687 ns |  1.0094 ns | 0.0553 ns |  0.47 |    0.00 | 0.0022 |      56 B |        1.00 |
| CreateChangeMetadata       | 29.042 ns |  0.6372 ns | 0.0349 ns |  1.00 |    0.00 | 0.0022 |      56 B |        1.00 |
