```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.04GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev    | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     30.23 ns |     3.962 ns |  0.217 ns |     1.22 |    0.01 | 0.0013 |      - |     112 B |        1.00 |
| DirectCall              |     24.78 ns |     3.979 ns |  0.218 ns |     1.00 |    0.01 | 0.0013 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 25,455.32 ns | 1,348.974 ns | 73.942 ns | 1,027.44 |    8.26 | 0.0610 | 0.0305 |    5287 B |       47.21 |
| GenericTypeConstruction |    100.58 ns |     5.086 ns |  0.279 ns |     4.06 |    0.03 | 0.0020 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     71.99 ns |     2.635 ns |  0.144 ns |     2.91 |    0.02 | 0.0020 |      - |     176 B |        1.57 |
